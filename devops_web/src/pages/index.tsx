import Image from 'next/image'
import { Inter } from 'next/font/google'
import { TodoItem } from '@/components/item/todo.item'
import { CreatedTodo } from '@/components/createdTodo/createdTodo'
import { useEffect, useState } from 'react';
import { v4 as uuidv4 } from 'uuid';
import { api } from './api/api';

// const inter = Inter({ subsets: ['latin'] })

interface Todo {
  id: string;
  description: string;
  isChecked: boolean;
}

export default function Home() {
  const [todos, setTodos] = useState<Todo[]>([]);

  useEffect(() => {
    (async () => {
      const { get, configInitial } = api();
      await configInitial();
      setTodos(await get());
    })();
  }, []);

  const handleAdd = async (description: string) => {
    const newTodo: Todo = {
      id: uuidv4(),
      description,
      isChecked: false
    }

    const { insert } = api();
    insert(newTodo);

    setTodos([...todos, { ...newTodo }]);
  }

  const handleChecked = async (id: string) => {
    console.log(id);

    const newTodos: Todo[] = todos.filter(async todo => {
      if (todo.id === id) {
        todo.isChecked = !todo.isChecked;
        const { update } = api();
        await update(id, todo.isChecked);
        return todo;
      }
      return todo;
    });

    setTodos(newTodos);
  }

  const handleRemove = async (id: string) => {
    const newTodos: Todo[] = todos.filter(todo => todo.id !== id);
    const { remove } = api();
    await remove(id);
    setTodos(newTodos);
  }

  return (
    <main className='flex items-center h-screen flex-col '>
      <h1 className='text-xl mt-4'>Lista de atividades</h1>
      <CreatedTodo handleAdd={handleAdd} />
      {todos.map(todo => <TodoItem todo={todo} key={todo.id} handleChecked={handleChecked} handleRemove={handleRemove} />)}
    </main>
  )
}
